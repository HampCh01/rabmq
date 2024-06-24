import json
import pika

connection = pika.BlockingConnection(pika.ConnectionParameters("localhost"))
channel = connection.channel()
channel.queue_declare(queue="tester")
jbody = {
    "input": {"random": "values", "diff": "keys", "means": "nothing"},
    "spiderarg2": r"\\test\location\foo",
}
channel.basic_publish(exchange="", routing_key="tester", body=json.dumps(jbody))
connection.close()
